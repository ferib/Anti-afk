# Anti-afk
This project is designed for World of Warcraft (8.0.1.27144)

# How it Works
The project will update LastHardwareAction every X seconds by the value of X.
This will be done inside Wow.exe, so we just have to inject a CodeCave and then CreateRemoteThread on it.
Please note that we need a Bypass in order to make use of CreateRemoteThread

# The CodeCave
```asm
0x90,                                                                                           //nop     ;Used for Bypass
0x55,                                                                                           //push rbp
0x48, 0x8B, 0xEC,                                                                               //mov rbp, rsp
0x48, 0xB9, 0xDE, 0xAD, 0xBE, 0xEF, 0xDE, 0xAD, 0xBE, 0xEF,                                     //rcx, LastHardwareAction
0x51,                                                                                           //push rcx
0x48, 0xB9, 0x71, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,                                     //rcx, 000000000000000271
0xFF, 0x15, 0x02, 0x00, 0x00, 0x00, 0xEB, 0X08, 0xDE, 0xAD, 0xBE, 0xEF, 0xDE, 0xAD, 0xBE, 0xEF, //call KERNERL32.Sleep
0x59,                                                                                           //pop rcx
0x48, 0x8B, 0x19,                                                                               //mov rbx,[rcx]
0x48, 0x81, 0xC3, 0x71, 0x02, 0x00, 0x00,                                                       //rbx,00000271
0x48, 0x89, 0x19,                                                                               //[rcx],rbx
0xEB, 0xD5,                                                                                     //jmp (-> push rcx)
0x48, 0x8B, 0xE5,                                                                               //mov rsp, rbp
0x5D,                                                                                           //pop rbp
0xC3                                                                                            //ret
```

# The Bypass
Blizzard will add a "0xC3" or "ret" instruction at the start of our CodeCave. No big deal if we wan't to use all code of our cave.
So a small work around is to start the Thread at CodeCave + 0x01.

in order to do this, we go to KERNEL32.BaseDumpAppcompatCacheWorker + 0x1E0, and see:
```asm
0xFF 0xE0       //jmp rax
0xCC            //int 3
0xCC            //int 3
0xCC            //int 3
```
So we know that RAX holds the start value of the Thread, so we patch the bytes to this:
```asm
0x48 0xFF 0xC0  //inc rax
0xFF 0xE0       //jmp rax
```

# LastHardwareAction
I have made a short video to show some stuff about the LastHardwareAction.
https://www.youtube.com/watch?v=o8J2HT-urkU&t=5s

# Donate
BTC: 1FeribRHR98Crux3DEZPXzjLBpfmHTHKqJ
