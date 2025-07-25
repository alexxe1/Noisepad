# Noisepad
An open-source soundpad application that lets you manipulate audio in real time with advanced features like pitch shifting, voice recording, and audio reversal.

---

## ⚠️ IMPORTANT
<b>Noisepad</b> requires a virtual microphone to play sounds in applications like <b>Discord</b> or <b>TeamSpeak</b>.  
I recommend using [VB-Cable](https://vb-audio.com/Cable) for this purpose.

> [!NOTE]
> When done installing [VB-Cable](https://vb-audio.com/Cable), make sure to have your speaker and microphone selected as default.

## How to Set Up [VB-Cable](https://vb-audio.com/Cable)

### Step 1: Open Sound Settings
1. Open the **Sound Settings** in Windows by searching for “Sound” in the Start Menu, or by pressing `Windows + R` and typing `mmsys.cpl`  
   <img width="401" height="454" alt="6d46a897-3877-4b33-bdc1-ed6a0bda8db3" src="https://github.com/user-attachments/assets/4c9f7d23-a631-424e-baa2-6de0c8ce2212" /> <br>

### Step 2: Select Your Microphone
2. In the **Recording** tab, find your microphone and double-click it  
   <img width="400" height="252" alt="image" src="https://github.com/user-attachments/assets/1b61c91d-9633-4e78-af5e-66f1dd9e1219" /> <br>

### Step 3: Enable "Listen to this device"
3. Go to the **Listen** tab, check the option **"Listen to this device"**, and select **"CABLE Input (VB-Audio Virtual Cable)"** from the dropdown below  
   <img width="362" height="308" alt="image" src="https://github.com/user-attachments/assets/91a5450b-3989-4aad-a7be-eb3736ffcf4a" /> <br>

### Step 4: Configure Noisepad
4. In **Noisepad**, open **Settings**, and under **Virtual microphone**, select **"CABLE Input (VB-Audio Virtual Cable)"**  
   <img width="764" height="134" alt="image" src="https://github.com/user-attachments/assets/f7f1a34d-0932-4e1e-905a-3cf0c7322914" /> <br>

---

## 🔈How do I use Noisepad with Discord or TeamSpeak (or any other application that uses a microphone)?
Just go to the application's settings and make sure to select `VB-Cable Output` as your microphone.

## 🐧 Can I use Noisepad on Linux or macOS?
Not yet. Noisepad currently depends on [NAudio](https://github.com/naudio/NAudio), a Windows-only audio library.  
However, since it's built with [Avalonia](https://avaloniaui.net/), cross-platform support *might* be possible in the future — community contributions are welcome!

## 🤝 Want to Contribute?

Absolutely! Noisepad is licensed under the MIT License — feel free to:

- Fork the project and submit pull requests
- Report bugs or request features via [Issues](#)
- Create your own version, even closed-source

Every contribution, big or small, is welcome.
