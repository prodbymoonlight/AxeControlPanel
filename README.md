# Quick disclaimer
Yes this readme will be replaced and written by myself, for now this is a quick placeholder. 

# 🛠️ AxeOneClockFixer (AXE I/O One ASIO Buffer Bypass)

A lightweight, high-performance open-source utility designed to bypass the notorious **`ASE_NoClock`** and `(no choices)` driver bug plaguing the **IK Multimedia AXE I/O One** audio interface on Windows 11.

Developed with a sleek, dark studio-plugin aesthetic. **100% Free, Open Source, and lightweight.**

---

## 🛑 The Problem
Many music producers face a frustrating glitch where the official IK Multimedia Control Panel locks up, displaying **`(no choices)`** in the buffer size menu, while DAWs like FL Studio throw latency errors. Under the hood, the proprietary driver panics and throws a Steinberg ASIO **`ASE_NoClock`** exception because it fails to detect an active hardware audio stream during initialization.

## 🔥 The Solution
This C# (.NET 8/9) utility bypasses the loggy official software entirely. By utilizing low-level COM interop pointers via `NAudio.Asio`, **AxeOneClockFixer** forces a manual `44.1 kHz` hardware clock trigger directly to the audio interface chip, forcing it to wake up. Once the clock cycles are stable, it dynamically queries the true hardware limits and allows you to force-inject your desired target buffer size directly into the Windows Registry before your DAW boots.

---

## ✨ Features
* **Zero Bloat:** No background services, no telemetry, no tracking.
* **Hardware Clock Enforcement:** Automatically breaks the `ASE_NoClock` hardware loop.
* **Dynamic Querying:** Reads actual sample limits straight from the interface.
* **Registry Injection:** Permanently veranks your preferred buffer settings into the Windows system registry.
* **Studio-Grade UI:** High-end, dark studio dashboard UI featuring subtle neon-glow indicators designed for dark control rooms.

---

## 🚀 How To Use
1. **Close your DAW** (FL Studio, Ableton, etc.) and your browser completely to release the driver.
2. Run `AxeOneClockFixer.exe`.
3. Select your target hardware buffer size from the dropdown (e.g., `128` for low-latency recording, `512` for heavy mixing).
4. Click **FORCE HARDWARE CLOCK & REGISTRY**.
5. Fire up your DAW and enjoy flawless, low-latency audio without hangups.

---

## 🛠️ Built With
* **C# / WPF** - Core framework and hardware interface.
* **NAudio.Asio** - Low-level ASIO driver integration wrapper.
* **Microsoft.Win32.Registry** - Direct system registry manipulation.

## 🤝 Contributing & License
This project is licensed under the MIT License - feel free to fork it, fix it, and share it with the production community. 

*Created for the community by a Dutch software development student. This one for the team!* 🫡🔥
