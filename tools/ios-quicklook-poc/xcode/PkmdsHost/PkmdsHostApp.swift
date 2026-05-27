import SwiftUI

@main
struct PkmdsHostApp: App {
    var body: some Scene {
        WindowGroup("PKMDS Quick Look (POC)") {
            ContentView()
        }
    }
}

struct ContentView: View {
    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 16) {
                Text("PKMDS Quick Look")
                    .font(.title.bold())
                Text("Proof of Concept")
                    .font(.title3)
                    .foregroundStyle(.secondary)

                Divider()

                Text("This host app exists so iOS will register the bundled Quick Look extensions. Once installed, long-press any supported file in **Files** (or any other Quick Look surface — Mail attachments, AirDrop received files) to preview it. In grid or column view, supported files also show custom thumbnails.")
                    .fixedSize(horizontal: false, vertical: true)

                Text("Supported types")
                    .font(.headline)
                    .padding(.top, 8)
                Text("• `.pk1`–`.pk9`, `.pa8`, `.pa9`, `.pb7`, `.pb8`, `.sk2`, `.ck3`, `.xk3`, `.bk4`, `.rk4` — Pokémon entity files")
                Text("• `.sav`, `.gci`, `.dsv`, `.srm`, `.dat`, `.bin`, `.fla` — Pokémon save files")
                Text("• `.wc3`–`.wc9`, `.wa8`, `.wa9`, `.wb7`, `.wb8`, `.wr7`, `.pcd`, `.pgt`, `.pgf` — Wonder Cards / Mystery Gifts")
            }
            .padding(24)
            .frame(maxWidth: .infinity, alignment: .leading)
        }
    }
}
