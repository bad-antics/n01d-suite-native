# N01D Suite Native

```
 ███╗   ██╗ ██████╗  ██╗██████╗       ███████╗██╗   ██╗██╗████████╗███████╗
 ████╗  ██║██╔═══██╗███║██╔══██╗      ██╔════╝██║   ██║██║╚══██╔══╝██╔════╝
 ██╔██╗ ██║██║   ██║╚██║██║  ██║█████╗███████╗██║   ██║██║   ██║   █████╗  
 ██║╚██╗██║██║   ██║ ██║██║  ██║╚════╝╚════██║██║   ██║██║   ██║   ██╔══╝  
 ██║ ╚████║╚██████╔╝ ██║██████╔╝      ███████║╚██████╔╝██║   ██║   ███████╗
 ╚═╝  ╚═══╝ ╚═════╝  ╚═╝╚═════╝       ╚══════╝ ╚═════╝ ╚═╝   ╚═╝   ╚══════╝
                    [ NATIVE WINDOWS DESKTOP TOOLS | bad-antics ]
```

Native Windows WPF desktop tools with hacker aesthetics. 13 apps, one solution.

> **🛰️ Flagship app: [N01D Overwatch](https://github.com/bad-antics/n01d-overwatch)** — Real-time conflict intelligence dashboard with 82+ OSINT feeds, flight tracking, war ops monitoring, missile defense mapping, and interactive tactical map. [Get it standalone →](https://github.com/bad-antics/n01d-overwatch)

## 🛠️ Tools Included (13 Apps)

| Tool | Description |
|------|-------------|
| **🛰️ [N01D-Overwatch](https://github.com/bad-antics/n01d-overwatch)** | **Conflict intelligence dashboard** — 82+ OSINT feeds, flight tracking, war ops, missile defense, eclipse warfare, interactive Leaflet.js map |
| **N01D-SysMon** | Real-time system monitor with CPU, memory, disk, network stats |
| **N01D-Notes** | Markdown note-taking with live preview |
| **N01D-Term** | Modern terminal emulator with tabs |
| **N01D-Forge** | Code snippet forge & template generator |
| **N01D-Media** | Media player & file manager |
| **N01D-Vault** | Encrypted password & secrets manager (AES-256) |
| **N01D-Pulse** | Network pulse monitor & port scanner |
| **N01D-Calc** | Hacker calculator (hex/bin/dec/oct + bitwise ops) |
| **N01D-Clip** | Clipboard history manager with search |
| **N01D-Cron** | Task scheduler & cron job manager |
| **N01D-Wipe** | Secure file & folder wiping (DoD 5220.22-M) |
| **N01D-Shared** | Shared theme library (N01D cyberpunk + XChat IRC palette) |

## ⚡ Features

### N01D-SysMon
- 📊 Real-time CPU usage graphs per core
- 🧠 Memory visualization with RAM/Swap
- 💾 Disk storage monitoring per partition
- 🌐 Network upload/download speeds
- 🔥 Top processes by CPU/Memory
- 🎨 Cyberpunk green aesthetics

### N01D-Notes
- 📝 Full markdown editor with syntax highlighting
- 👁️ Live preview with WebView2
- 📂 File management (New/Open/Save)
- ⌨️ Keyboard shortcuts (Ctrl+S, Ctrl+N, etc.)
- 🔗 GFM support (tables, code blocks, etc.)
- 📊 Word/line/character count

### N01D-Term
- 🖥️ Full PowerShell integration
- 📑 Multi-tab support
- 📜 Persistent command history
- ⚡ Quick commands sidebar
- 🎨 Matrix-style aesthetics
- ⌨️ Tab completion & history navigation

## 🚀 Build & Run

### Prerequisites
- .NET 8.0 SDK
- Windows 10/11
- Visual Studio 2022 (optional)

### Build
```powershell
cd n01d-suite-native
dotnet restore
dotnet build --configuration Release
```

### Run Individual Apps
```powershell
# System Monitor
dotnet run --project N01D.SysMon

# Notes
dotnet run --project N01D.Notes

# Terminal
dotnet run --project N01D.Term
```

### Publish as Single Executables
```powershell
dotnet publish N01D.SysMon -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
dotnet publish N01D.Notes -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
dotnet publish N01D.Term -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

## ⌨️ Keyboard Shortcuts

### N01D-Notes
| Shortcut | Action |
|----------|--------|
| `Ctrl+N` | New note |
| `Ctrl+O` | Open note |
| `Ctrl+S` | Save note |
| `Ctrl+Shift+S` | Save as |
| `Ctrl+P` | Toggle preview |

### N01D-Term
| Shortcut | Action |
|----------|--------|
| `Ctrl+T` | New tab |
| `Ctrl+W` | Close tab |
| `Ctrl+Tab` | Next tab |
| `Ctrl+Shift+Tab` | Previous tab |
| `Ctrl+L` | Clear screen |
| `↑/↓` | History navigation |

## 🎨 Theme

All apps use the N01D cyberpunk theme:
- **Background:** `#0D0D0D` (Dark)
- **Primary:** `#00FF41` (Matrix Green)
- **Secondary:** `#0ABDC6` (Cyan)
- **Accent:** `#FF0055` (Neon Pink)
- **Font:** Consolas / Cascadia Mono

## 🔗 Part of the N01D Suite

| Tool | Platform | Status |
|------|----------|--------|
| [N01D-Overwatch](https://github.com/bad-antics/n01d-overwatch) | Windows Native | ✅ **Flagship** — [Standalone Repo](https://github.com/bad-antics/n01d-overwatch) |
| N01D-SysMon | Windows Native | ✅ Complete |
| N01D-Notes | Windows Native | ✅ Complete |
| N01D-Term | Windows Native | ✅ Complete |
| N01D-Forge | Windows Native | ✅ Complete |
| N01D-Media | Windows Native | ✅ Complete |
| N01D-Vault | Windows Native | ✅ Complete |
| N01D-Pulse | Windows Native | ✅ Complete |
| N01D-Calc | Windows Native | ✅ Complete |
| N01D-Clip | Windows Native | ✅ Complete |
| N01D-Cron | Windows Native | ✅ Complete |
| N01D-Wipe | Windows Native | ✅ Complete |

## 📜 License

MIT License - Part of the NullSec Framework

---

[GitHub](https://github.com/bad-antics) • [NullSec](https://github.com/bad-antics/nullsec) • [Issues](https://github.com/bad-antics/n01d-suite-native/issues)

**Made by [bad-antics](https://github.com/bad-antics)**
