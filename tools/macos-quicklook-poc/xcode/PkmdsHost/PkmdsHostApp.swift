import SwiftUI

@main
struct PkmdsHostApp: App {
    var body: some Scene {
        WindowGroup("PKMDS Quick Look (POC)") {
            ContentView()
                .frame(minWidth: 420, minHeight: 220)
        }
    }
}

struct ContentView: View {
    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 12) {
                Text("PKMDS Quick Look — Proof of Concept")
                    .font(.title2.bold())
                Text("This host app exists so macOS will register the bundled Quick Look extensions. Press Space on any supported file in Finder to preview it. Finder icon view also shows custom thumbnails.")
                    .foregroundStyle(.secondary)
                    .fixedSize(horizontal: false, vertical: true)

                Text("Supported types")
                    .font(.headline)
                    .padding(.top, 4)
                Text("• `.pk1`–`.pk9`, `.pa8`, `.pa9`, `.pb7`, `.pb8`, `.sk2`, `.ck3`, `.xk3`, `.bk4`, `.rk4` — Pokémon entity files")
                Text("• `.sav`, `.gci`, `.dsv`, `.srm`, `.dat`, `.bin`, `.fla` — Pokémon save files")
                Text("• `.wc3`–`.wc9`, `.wa8`, `.wa9`, `.wb7`, `.wb8`, `.wr7`, `.pcd`, `.pgt`, `.pgf` — Wonder Cards / Mystery Gifts")
            }
            .padding(24)
            .frame(maxWidth: .infinity, alignment: .leading)
        }
    }
}
