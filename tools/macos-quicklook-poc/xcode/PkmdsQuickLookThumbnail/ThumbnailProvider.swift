import Cocoa
import QuickLookThumbnailing

// NativeAOT symbols from PkmdsNative.dylib ─────────────────────────────────────────────────────

@_silgen_name("pkmds_get_sprite_path")
private func pkmds_get_sprite_path(
    _ data: UnsafePointer<UInt8>?, _ length: Int32,
    _ ext: UnsafePointer<UInt8>?, _ extLen: Int32,
    _ outPath: UnsafeMutablePointer<UInt8>?, _ outCap: Int32
) -> Int32

@_silgen_name("pkmds_describe_save")
private func pkmds_describe_save(
    _ data: UnsafePointer<UInt8>?, _ length: Int32,
    _ outJson: UnsafeMutablePointer<UInt8>?, _ outCap: Int32
) -> Int32

private let kPathCap = 4 * 1024
private let kJsonCap = 64 * 1024

// MARK: - Provider ──────────────────────────────────────────────────────────────────────────────

final class ThumbnailProvider: QLThumbnailProvider {

    // NativeAOT runtime startup costs 2–4 s on the first call. Pre-warm it in a background
    // queue as soon as the extension process spawns so the budget is available for processing.
    private static let warmupGroup: DispatchGroup = {
        let g = DispatchGroup()
        g.enter()
        DispatchQueue.global(qos: .userInitiated).async {
            _ = pkmds_get_sprite_path(nil, 0, nil, 0, nil, 0)  // no-op; initialises the runtime
            g.leave()
        }
        return g
    }()

    override init() {
        super.init()
        _ = ThumbnailProvider.warmupGroup  // trigger the lazy static init immediately
    }

    override func provideThumbnail(for request: QLFileThumbnailRequest,
                                   _ handler: @escaping (QLThumbnailReply?, Error?) -> Void) {
        DispatchQueue.global(qos: .userInitiated).async {
            // Wait for the NativeAOT warmup (≤4 s), leaving ≥1 s for processing.
            guard ThumbnailProvider.warmupGroup.wait(timeout: .now() + 4) == .success else {
                handler(nil, nil); return
            }

            guard let fileData = try? Data(contentsOf: request.fileURL) else {
                handler(nil, nil); return
            }

            let size     = request.maximumSize
            let ext      = request.fileURL.pathExtension.lowercased()
            let saveInfo = parseSave(fileData)

            // Pre-compute everything that touches NSImage / CGImage here, on our controlled
            // background queue. The QLThumbnailReply drawing closure runs on a system-managed
            // thread where calling image.cgImage() can deadlock with AppKit internals.
            struct SpriteData { let image: NSImage; let src: NSRect }
            let spriteData: SpriteData? = saveInfo == nil ? {
                let rel = spritePath(data: fileData, ext: ext) ?? "a/a_unknown.png"
                guard let img = loadBundledSprite(rel) else { return nil }
                return SpriteData(image: img, src: opaqueSourceRect(img))
            }() : nil

            // QLThumbnailReply(contextSize:drawing:) is available on macOS 10.15+.
            // The system calls the block with an already-configured CGContext. We wrap it in
            // NSGraphicsContext so we can use AppKit drawing (NSBezierPath, NSAttributedString, etc.).
            let reply = QLThumbnailReply(contextSize: size) { (ctx: CGContext) -> Bool in
                NSGraphicsContext.saveGraphicsState()
                defer { NSGraphicsContext.restoreGraphicsState() }
                NSGraphicsContext.current = NSGraphicsContext(cgContext: ctx, flipped: false)

                // ctx dimensions are in pixels (2× the point size on Retina); use them for layout.
                let ctxSize = CGSize(width: CGFloat(ctx.width), height: CGFloat(ctx.height))

                if let info = saveInfo {
                    drawTrainerCard(size: ctxSize, info: info)
                } else if let sd = spriteData, sd.src.width > 0, sd.src.height > 0 {
                    let fill: CGFloat = 0.90
                    let scale    = min(ctxSize.width * fill / sd.src.width,
                                      ctxSize.height * fill / sd.src.height)
                    let drawSize = NSSize(width: sd.src.width * scale, height: sd.src.height * scale)
                    let origin   = NSPoint(x: (ctxSize.width  - drawSize.width)  / 2,
                                          y: (ctxSize.height - drawSize.height) / 2)
                    sd.image.draw(in:    NSRect(origin: origin, size: drawSize),
                                  from:  sd.src,
                                  operation: .sourceOver,
                                  fraction:  1.0)
                }
                return true
            }
            handler(reply, nil)
        }
    }
}

// MARK: - Dylib bridge ──────────────────────────────────────────────────────────────────────────

private func spritePath(data: Data, ext: String) -> String? {
    let extBytes = Array(ext.utf8)
    var buf = [UInt8](repeating: 0, count: kPathCap)
    let n = data.withUnsafeBytes { raw -> Int32 in
        let ptr = raw.bindMemory(to: UInt8.self).baseAddress
        return extBytes.withUnsafeBufferPointer { extBuf in
            buf.withUnsafeMutableBufferPointer { dst in
                pkmds_get_sprite_path(ptr, Int32(data.count),
                                      extBuf.baseAddress, Int32(extBytes.count),
                                      dst.baseAddress, Int32(kPathCap))
            }
        }
    }
    guard n > 0 else { return nil }
    return String(decoding: buf.prefix(Int(n)), as: UTF8.self)
}

private struct SaveInfo {
    let version: String
    let ot: String
    let tid: Int
    let sid: Int
    let hours: Int
    let minutes: Int
}

private func parseSave(_ data: Data) -> SaveInfo? {
    var buf = [UInt8](repeating: 0, count: kJsonCap)
    let n = data.withUnsafeBytes { raw -> Int32 in
        let ptr = raw.bindMemory(to: UInt8.self).baseAddress
        return buf.withUnsafeMutableBufferPointer { dst in
            pkmds_describe_save(ptr, Int32(data.count), dst.baseAddress, Int32(kJsonCap))
        }
    }
    guard n > 0,
          let json = try? JSONSerialization.jsonObject(with: Data(buf.prefix(Int(n)))) as? [String: Any]
    else { return nil }

    return SaveInfo(
        version: json["version"] as? String ?? "?",
        ot:      json["ot"]      as? String ?? "",
        tid:     json["tid"]     as? Int    ?? 0,
        sid:     json["sid"]     as? Int    ?? 0,
        hours:   json["playedHours"]   as? Int ?? 0,
        minutes: json["playedMinutes"] as? Int ?? 0
    )
}

// MARK: - Sprite drawing ────────────────────────────────────────────────────────────────────────
//
// Sprites are copied into the extension bundle at Resources/sprites/ by build-extension.sh.
// The PNG files include significant transparent padding; opaqueSourceRect trims it so the
// visible Pokémon fills the thumbnail rather than appearing tiny in a sea of transparency.
// Sprite loading and opaque-rect computation happen on our background queue (before the
// QLThumbnailReply drawing closure) because image.cgImage() can deadlock on AppKit's internal
// cache lock when called from the system-managed thread that invokes the drawing closure.

// Returns the tight bounding box of non-transparent pixels in NSImage (bottom-left) coords.
// Falls back to the full image rect when pixel data is unavailable.
private func opaqueSourceRect(_ image: NSImage) -> NSRect {
    let imgSize = image.size
    let full = NSRect(origin: .zero, size: imgSize)
    guard let cgImg = image.cgImage(forProposedRect: nil, context: nil, hints: nil) else { return full }
    let pw = cgImg.width, ph = cgImg.height
    guard pw > 0, ph > 0 else { return full }

    // Let CGContext own its backing store (data: nil) so storage lifetime is managed by Core Graphics.
    // BGRA/premultipliedFirst is the native format on ARM; alpha lands at byte offset +3.
    let stride = pw * 4
    guard let ctx = CGContext(
        data: nil, width: pw, height: ph,
        bitsPerComponent: 8, bytesPerRow: stride,
        space: CGColorSpaceCreateDeviceRGB(),
        bitmapInfo: CGBitmapInfo.byteOrder32Little.rawValue | CGImageAlphaInfo.premultipliedFirst.rawValue
    ) else { return full }
    // CGBitmapContext stores rows in raster order: row 0 = visual TOP (same as CGImage).
    // This is the opposite of NSImage's coordinate system (y=0 at bottom).
    ctx.draw(cgImg, in: CGRect(x: 0, y: 0, width: pw, height: ph))
    guard let rawData = ctx.data else { return full }
    let bytes = rawData.assumingMemoryBound(to: UInt8.self)

    var left = pw, right = 0, minRow = ph, maxRow = 0
    for row in 0..<ph {
        for col in 0..<pw {
            if bytes[row * stride + col * 4 + 3] > 10 {
                if col < left   { left   = col }
                if col > right  { right  = col }
                if row < minRow { minRow = row }
                if row > maxRow { maxRow = row }
            }
        }
    }
    guard right >= left, maxRow >= minRow else { return full }

    // CGBitmapContext row 0 is the visual BOTTOM (y=0 in NSImage coords).
    // minRow = smallest buffer row with opaque pixels = visual top of content.
    // NSImage y of the bottom edge of content = (ph - maxRow - 1) * sy.
    let sx = imgSize.width  / CGFloat(pw)
    let sy = imgSize.height / CGFloat(ph)
    return NSRect(
        x:      CGFloat(left)              * sx,
        y:      CGFloat(ph - maxRow - 1)   * sy,
        width:  CGFloat(right - left + 1)  * sx,
        height: CGFloat(maxRow - minRow + 1) * sy
    )
}

private func loadBundledSprite(_ relativePath: String) -> NSImage? {
    guard let resourceURL = Bundle.main.resourceURL else { return nil }
    var url = resourceURL.appendingPathComponent("sprites")
    for component in relativePath.split(separator: "/").map(String.init) {
        url = url.appendingPathComponent(component)
    }
    return NSImage(contentsOf: url)
}

// MARK: - Trainer card ──────────────────────────────────────────────────────────────────────────
//
// Mirrors Windows SaveCard.cs layout. Windows uses top-left origin (y down); AppKit uses
// bottom-left origin (y up). Conversion: appkitY = canvasHeight − windowsY − elementHeight.

private func drawTrainerCard(size: CGSize, info: SaveInfo) {
    let margin      = size.width * 0.05
    let cardRect    = NSRect(x: margin, y: margin,
                             width:  size.width  - 2 * margin,
                             height: size.height - 2 * margin)
    let radius      = size.width * 0.10
    let borderWidth = max(2.0, size.width * 0.014)

    let (accent, accent2) = accentColors(version: info.version)
    let dark              = NSColor(calibratedRed: 0.13, green: 0.13, blue: 0.13, alpha: 1)
    let muted             = NSColor(calibratedRed: 0.40, green: 0.40, blue: 0.40, alpha: 1)

    // ── Card background ───────────────────────────────────────────────────────────────────────
    let cardPath = NSBezierPath(roundedRect: cardRect, xRadius: radius, yRadius: radius)
    NSColor.white.setFill()
    cardPath.fill()

    // Accent border (single colour for all groups; the RB/GS gradient is a cosmetic enhancement).
    cardPath.lineWidth = borderWidth
    accent.setStroke()
    cardPath.stroke()

    // ── Version code (large, accent, top 39% of the card) ────────────────────────────────────
    // Windows: y = rect.Y + size*0.05, height = size*0.34
    // AppKit:  y = size − (margin + 0.05·size + 0.34·size) = size·0.61 − margin
    let codeH    = size.height * 0.34
    let codeY    = size.height - (margin + size.height * 0.05 + codeH)
    let codeArea = NSRect(x: cardRect.minX, y: codeY, width: cardRect.width, height: codeH)

    if let a2 = accent2 {
        drawFittedGradientText(info.version, in: codeArea,
                               maxEm: size.height * 0.40, bold: true, a1: accent, a2: a2)
    } else {
        drawFittedText(info.version, in: codeArea, maxEm: size.height * 0.40, bold: true, color: accent)
    }

    // ── OT name ──────────────────────────────────────────────────────────────────────────────
    // Windows: y = rect.Y + size*0.43, height = size*0.15
    let otH    = size.height * 0.15
    let otY    = size.height - (margin + size.height * 0.43 + otH)
    let otPad  = cardRect.width * 0.05
    let otArea = NSRect(x: cardRect.minX + otPad, y: otY,
                        width: cardRect.width - 2 * otPad, height: otH)
    drawFittedText(info.ot.isEmpty ? "Trainer" : info.ot,
                   in: otArea, maxEm: size.height * 0.135, bold: true, color: dark)

    // ── TID/SID ──────────────────────────────────────────────────────────────────────────────
    // Windows: y = rect.Y + size*0.60, height = size*0.12
    let idsH    = size.height * 0.12
    let idsY    = size.height - (margin + size.height * 0.60 + idsH)
    let idsArea = NSRect(x: cardRect.minX, y: idsY, width: cardRect.width, height: idsH)
    drawFittedText("\(info.tid)/\(info.sid)", in: idsArea,
                   maxEm: size.height * 0.10, bold: false, color: muted)

    // ── Playtime ──────────────────────────────────────────────────────────────────────────────
    // Windows: y = rect.Y + size*0.73, height = size*0.12
    let playH    = size.height * 0.12
    let playY    = size.height - (margin + size.height * 0.73 + playH)
    let playArea = NSRect(x: cardRect.minX, y: playY, width: cardRect.width, height: playH)
    drawFittedText(String(format: "%d:%02d", info.hours, info.minutes),
                   in: playArea, maxEm: size.height * 0.10, bold: false, color: muted)
}

// MARK: - Text helpers ──────────────────────────────────────────────────────────────────────────

// Shrink the system font until `text` fits inside `rect`, then draw it horizontally centred.
private func drawFittedText(_ text: String, in rect: NSRect, maxEm: CGFloat, bold: Bool, color: NSColor) {
    let style = NSMutableParagraphStyle()
    style.alignment = .center

    var em = maxEm
    while em > 6 {
        let attrs = makeAttrs(em: em, bold: bold, color: color, style: style)
        let sz = (text as NSString).boundingRect(
            with: NSSize(width: rect.width, height: .greatestFiniteMagnitude),
            options: .usesLineFragmentOrigin, attributes: attrs).size
        if sz.width <= rect.width && sz.height <= rect.height {
            (text as NSString).draw(in: vertCentred(sz, in: rect), withAttributes: attrs)
            return
        }
        em -= 1
    }
    let attrs = makeAttrs(em: 6, bold: bold, color: color, style: style)
    let sz    = (text as NSString).boundingRect(
        with: NSSize(width: rect.width, height: .greatestFiniteMagnitude),
        options: .usesLineFragmentOrigin, attributes: attrs).size
    (text as NSString).draw(in: vertCentred(sz, in: rect), withAttributes: attrs)
}

// Two-colour horizontal gradient text — each character is coloured by linear interpolation.
// Used for the RB ("R"=red, "B"=blue) and GS ("G"=gold, "S"=silver) ambiguous groups.
private func drawFittedGradientText(_ text: String, in rect: NSRect,
                                    maxEm: CGFloat, bold: Bool, a1: NSColor, a2: NSColor) {
    guard !text.isEmpty else { return }
    let chars = Array(text)

    var em = maxEm
    while em > 6 {
        let attrs = makeAttrs(em: em, bold: bold, color: a1, style: nil)
        let sz = (text as NSString).boundingRect(
            with: NSSize(width: rect.width, height: .greatestFiniteMagnitude),
            options: .usesLineFragmentOrigin, attributes: attrs).size
        if sz.width <= rect.width && sz.height <= rect.height { break }
        em -= 1
    }

    // Place each character with its interpolated colour.
    let baseAttrs = makeAttrs(em: em, bold: bold, color: a1, style: nil)
    var curX = rect.midX - (text as NSString).size(withAttributes: baseAttrs).width / 2

    for (i, ch) in chars.enumerated() {
        let t     = chars.count > 1 ? CGFloat(i) / CGFloat(chars.count - 1) : 0.0
        let color = interpolate(from: a1, to: a2, t: t)
        let attrs = makeAttrs(em: em, bold: bold, color: color, style: nil)
        let str   = String(ch) as NSString
        let sz    = str.size(withAttributes: attrs)
        str.draw(at: NSPoint(x: curX, y: rect.midY - sz.height / 2), withAttributes: attrs)
        curX += sz.width
    }
}

private func makeAttrs(em: CGFloat, bold: Bool, color: NSColor,
                       style: NSParagraphStyle?) -> [NSAttributedString.Key: Any] {
    var a: [NSAttributedString.Key: Any] = [
        .font: NSFont.systemFont(ofSize: em, weight: bold ? .bold : .regular),
        .foregroundColor: color,
    ]
    if let style { a[.paragraphStyle] = style }
    return a
}

private func vertCentred(_ sz: NSSize, in rect: NSRect) -> NSRect {
    NSRect(x: rect.minX, y: rect.midY - sz.height / 2, width: rect.width, height: sz.height)
}

// MARK: - Accent colours ────────────────────────────────────────────────────────────────────────
//
// Mirrors Windows SaveCard.cs AccentColor / AccentColors (curated per-GameVersion palette).
// The two ambiguous groups (RB and GS) return two colours for the gradient paths above.
// All other versions return a single colour with nil second.

private func accentColors(version: String) -> (NSColor, NSColor?) {
    switch version {
    case "RB": return (hex(0xC0392B), hex(0x2980B9)) // Red + Blue
    case "GS": return (hex(0xD4860B), hex(0x7F8C9A)) // Gold + Silver
    default:   return (accentColor(version: version), nil)
    }
}

private func accentColor(version: String) -> NSColor {
    switch version {
    case "RD", "R", "FR", "OR":   return hex(0xC0392B) // red
    case "SL":                     return hex(0xD0432A) // scarlet
    case "GN", "LG":              return hex(0x27AE60) // green
    case "E":                     return hex(0x1E9E6A) // emerald
    case "BU", "S", "AS", "X":   return hex(0x2980B9) // blue
    case "D", "BD":               return hex(0x4A78B0) // diamond
    case "P", "SP":               return hex(0xC56AA0) // pearl
    case "Pt":                    return hex(0x7A6FA0) // platinum
    case "YW", "GP":              return hex(0xC79100) // yellow / let's go pikachu
    case "GE":                    return hex(0x8B5A2B) // let's go eevee
    case "GD", "HG":              return hex(0xD4860B) // gold
    case "SI", "SS":              return hex(0x7F8C9A) // silver
    case "C":                     return hex(0x16A0A0) // crystal
    case "B", "B2":               return hex(0x333333) // black
    case "W", "W2":               return hex(0x8A8A8A) // white
    case "SN", "US":              return hex(0xE07B2A) // sun
    case "MN", "UM":              return hex(0x5B4B8A) // moon
    case "SW":                    return hex(0x1F9BC2) // sword
    case "SH":                    return hex(0xC0306A) // shield
    case "PLA":                   return hex(0x2FA37A) // legends: arceus
    case "VL":                    return hex(0x7A3FA0) // violet
    case "Y":                     return hex(0xC0392B) // yellow (gen 1 → red family)
    default:                      return hex(0x3B6EA5) // neutral blue
    }
}

private func hex(_ rgb: Int) -> NSColor {
    NSColor(calibratedRed:   CGFloat((rgb >> 16) & 0xFF) / 255,
            green: CGFloat((rgb >>  8) & 0xFF) / 255,
            blue:  CGFloat( rgb        & 0xFF) / 255,
            alpha: 1)
}

private func interpolate(from a: NSColor, to b: NSColor, t: CGFloat) -> NSColor {
    NSColor(calibratedRed:   a.redComponent   + (b.redComponent   - a.redComponent)   * t,
            green: a.greenComponent + (b.greenComponent - a.greenComponent) * t,
            blue:  a.blueComponent  + (b.blueComponent  - a.blueComponent)  * t,
            alpha: 1)
}
