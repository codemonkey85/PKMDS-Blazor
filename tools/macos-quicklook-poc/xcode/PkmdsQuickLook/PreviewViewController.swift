import Cocoa
import Quartz
import WebKit

@_silgen_name("pkmds_render_pkm_html")
private func pkmds_render_pkm_html(
    _ data: UnsafePointer<UInt8>?, _ length: Int32,
    _ outHtml: UnsafeMutablePointer<UInt8>?, _ outCap: Int32
) -> Int32

@_silgen_name("pkmds_render_save_html")
private func pkmds_render_save_html(
    _ data: UnsafePointer<UInt8>?, _ length: Int32,
    _ outHtml: UnsafeMutablePointer<UInt8>?, _ outCap: Int32
) -> Int32

// Auto-dispatching renderer: ext is the file extension without the leading dot (e.g. "wb8").
@_silgen_name("pkmds_render_file_html")
private func pkmds_render_file_html(
    _ data: UnsafePointer<UInt8>?, _ length: Int32,
    _ ext: UnsafePointer<UInt8>?, _ extLen: Int32,
    _ outHtml: UnsafeMutablePointer<UInt8>?, _ outCap: Int32
) -> Int32

private let outputCapacity = 256 * 1024

final class PreviewViewController: NSViewController, QLPreviewingController {
    private var webView: WKWebView!

    override func loadView() {
        let config = WKWebViewConfiguration()
        let webView = WKWebView(frame: .zero, configuration: config)
        webView.translatesAutoresizingMaskIntoConstraints = false
        webView.setValue(false, forKey: "drawsBackground")
        self.webView = webView
        self.view = webView
    }

    func preparePreviewOfFile(at url: URL, completionHandler: @escaping (Error?) -> Void) {
        do {
            let data = try Data(contentsOf: url)
            let html = try render(data: data, url: url)
            webView.loadHTMLString(html, baseURL: nil)
            completionHandler(nil)
        } catch {
            completionHandler(error)
        }
    }

    private func render(data: Data, url: URL) throws -> String {
        let ext = url.pathExtension.lowercased()
        let extBytes = Array(ext.utf8)

        var outBuf = [UInt8](repeating: 0, count: outputCapacity)
        let written = data.withUnsafeBytes { (raw: UnsafeRawBufferPointer) -> Int32 in
            let ptr = raw.bindMemory(to: UInt8.self).baseAddress
            return extBytes.withUnsafeBufferPointer { extBuf in
                outBuf.withUnsafeMutableBufferPointer { dst in
                    pkmds_render_file_html(
                        ptr, Int32(data.count),
                        extBuf.baseAddress, Int32(extBytes.count),
                        dst.baseAddress, Int32(outputCapacity))
                }
            }
        }

        guard written > 0 else {
            throw NSError(
                domain: "com.bondcodes.pkmds.quicklook",
                code: Int(written),
                userInfo: [NSLocalizedDescriptionKey: "renderer returned \(written)"])
        }
        return String(decoding: outBuf.prefix(Int(written)), as: UTF8.self)
    }
}
