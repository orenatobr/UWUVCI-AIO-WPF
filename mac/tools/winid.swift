import CoreGraphics
import Foundation
// usage: winid <pid>  -> prints the CGWindowID of the largest on-screen window owned by that pid
let pid = Int32(CommandLine.arguments[1])!
let opts: CGWindowListOption = [.optionOnScreenOnly, .excludeDesktopElements]
guard let list = CGWindowListCopyWindowInfo(opts, kCGNullWindowID) as? [[String: Any]] else { exit(1) }
var best: (Int, Double) = (0, 0)
for w in list {
  guard (w[kCGWindowOwnerPID as String] as? Int32) == pid,
        (w[kCGWindowLayer as String] as? Int) == 0,
        let b = w[kCGWindowBounds as String] as? [String: Double],
        let id = w[kCGWindowNumber as String] as? Int else { continue }
  let area = (b["Width"] ?? 0) * (b["Height"] ?? 0)
  if area > best.1 { best = (id, area) }
}
if best.0 == 0 { exit(2) }
print(best.0)
