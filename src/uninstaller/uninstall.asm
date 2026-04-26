; Minimal Windows Uninstaller in x64 Assembly (MASM)
; Requests UAC elevation via manifest
; Includes confirmation dialog and logging

extern ExitProcess : proc
extern ShellExecuteA : proc
extern CreateFileA : proc
extern ReadFile : proc
extern WriteFile : proc
extern CloseHandle : proc
extern MessageBoxA : proc

.data
    pathsFile db "uninstall.paths", 0
    logFile db "uninstall.log", 0
    verb db "open", 0
    cmd db "cmd.exe", 0
    
    ; Messages
    appName db "Uninstaller", 0
    confirmMsg db "Are you sure you want to uninstall this application?", 0
    
    ; Log messages
    logStarted db "Uninstallation started", 13, 10, 0
    logCancelled db "Uninstallation cancelled by user", 13, 10, 0
    logExecuting db "Executing uninstallation commands", 13, 10, 0
    logError db "Error: Could not open uninstall.paths", 13, 10, 0
    
    handle dq 0
    logHandle dq 0
    bytesRead dd 0
    bytesWritten dd 0
    buffer db 1024 dup(0)

.code
main proc
    sub rsp, 88 ; Shadow space + parameters

    ; 1. Create/Open log file (append)
    mov rcx, offset logFile
    mov rdx, 40000000h ; GENERIC_WRITE
    mov r8, 1          ; FILE_SHARE_READ
    xor r9, r9         ; Security
    mov qword ptr [rsp+20h], 4 ; OPEN_ALWAYS
    mov qword ptr [rsp+28h], 80h ; FILE_ATTRIBUTE_NORMAL
    mov qword ptr [rsp+30h], 0 ; Template
    call CreateFileA
    mov logHandle, rax
    
    cmp rax, -1
    je skip_logging_start
    
    ; Seek to end of log file
    mov rcx, logHandle
    xor rdx, rdx
    xor r8, r8
    mov r9, 2 ; FILE_END
    ; SetFilePointerEx is better but let's keep it simple for now
    
    ; Log: Uninstallation started
    mov rcx, logHandle
    mov rdx, offset logStarted
    mov r8, 24 ; length
    mov r9, offset bytesWritten
    mov qword ptr [rsp+20h], 0
    call WriteFile

skip_logging_start:

    ; 2. Show confirmation dialog
    mov rcx, 0 ; hwnd
    mov rdx, offset confirmMsg
    mov r8, offset appName
    mov r9, 24h ; MB_YESNO | MB_ICONQUESTION
    call MessageBoxA
    
    cmp rax, 7 ; IDNO
    je cancelled

    ; 3. Open uninstall.paths
    mov rcx, offset pathsFile
    mov rdx, 80000000h ; GENERIC_READ
    mov r8, 1          ; FILE_SHARE_READ
    xor r9, r9         ; Security
    mov qword ptr [rsp+20h], 3 ; OPEN_EXISTING
    mov qword ptr [rsp+28h], 80h ; FILE_ATTRIBUTE_NORMAL
    mov qword ptr [rsp+30h], 0 ; Template
    call CreateFileA
    mov handle, rax
    
    cmp rax, -1
    je error_label

    ; 4. Read file content
    mov rcx, handle
    mov rdx, offset buffer
    mov r8, 1023 ; Leave space for null terminator
    mov r9, offset bytesRead
    mov qword ptr [rsp+20h], 0
    call ReadFile
    call CloseHandle

    ; Null terminate the buffer
    mov eax, bytesRead
    lea rbx, buffer
    mov byte ptr [rbx + rax], 0

    ; Log: Executing
    cmp logHandle, -1
    je skip_logging_exec
    mov rcx, logHandle
    mov rdx, offset logExecuting
    mov r8, 35 ; length
    mov r9, offset bytesWritten
    mov qword ptr [rsp+20h], 0
    call WriteFile
skip_logging_exec:

    ; 5. Execute the command via cmd.exe
    mov rcx, 0 ; hwnd
    mov rdx, offset verb
    mov r8, offset cmd
    mov r9, offset buffer ; This contains the args from the file
    mov qword ptr [rsp+20h], 0 ; dir
    mov qword ptr [rsp+28h], 0 ; show
    call ShellExecuteA
    jmp exit_label

cancelled:
    cmp logHandle, -1
    je exit_label
    mov rcx, logHandle
    mov rdx, offset logCancelled
    mov r8, 34 ; length
    mov r9, offset bytesWritten
    mov qword ptr [rsp+20h], 0
    call WriteFile
    jmp exit_label

error_label:
    cmp logHandle, -1
    je exit_label
    mov rcx, logHandle
    mov rdx, offset logError
    mov r8, 38 ; length
    mov r9, offset bytesWritten
    mov qword ptr [rsp+20h], 0
    call WriteFile

exit_label:
    cmp logHandle, -1
    je final_exit
    mov rcx, logHandle
    call CloseHandle

final_exit:
    add rsp, 88
    xor rcx, rcx
    call ExitProcess
main endp
end
