import CoreGraphics
import Foundation
// usage: sendkey <pid> <key> [hold-ms]   key: a letter (a-z), "return", "space", "up", "down", "left", "right"
// Posts a key down and up straight to one process, so Cemu gets the key without being frontmost.
let codes: [String: CGKeyCode] = [
  "a": 0, "s": 1, "d": 2, "f": 3, "h": 4, "g": 5, "z": 6, "x": 7, "c": 8, "v": 9, "b": 11, "q": 12, "w": 13,
  "e": 14, "r": 15, "y": 16, "t": 17, "o": 31, "u": 32, "i": 34, "p": 35, "l": 37, "j": 38, "k": 40, "n": 45,
  "m": 46, "return": 36, "space": 49, "left": 123, "right": 124, "down": 125, "up": 126,
]
let a = CommandLine.arguments
guard a.count >= 3, let pid = Int32(a[1]), let code = codes[a[2].lowercased()] else {
  FileHandle.standardError.write(Data("usage: sendkey <pid> <key> [hold-ms]\n".utf8)); exit(2)
}
let hold = a.count > 3 ? (Double(a[3]) ?? 150) : 150
for down in [true, false] {
  guard let e = CGEvent(keyboardEventSource: nil, virtualKey: code, keyDown: down) else { exit(1) }
  e.postToPid(pid)
  if down { usleep(UInt32(hold * 1000)) }
}
