; Made And Checked By DELTA SYNTH & Gemini AI
; Credit: DELTA SYNTH & Gemini, UTAU THAILAND, stakira, Ferina and Printmov
; Version: 1.2

ManifestDPIAware true

; --- ข้อมูลพื้นฐานของโปรแกรม ---
!define PRODUCT_NAME "OpenUtau"
!define PRODUCT_PUBLISHER "stakira"
!define PRODUCT_WEB_SITE "https://www.openutau.com"
!define PRODUCT_UNINST_KEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}"
!define PRODUCT_UNINST_ROOT_KEY "HKLM"

; MUI 2.0 compatible
!include "MUI2.nsh"

; --- การตั้งค่าอินเทอร์เฟซ (MUI Settings) ---
!define MUI_ABORTWARNING
!define MUI_ICON "OpenUtau\Assets\open-utau.ico"
!define MUI_UNICON "${NSISDIR}\Contrib\Graphics\Icons\modern-uninstall.ico"

; การตั้งค่าภาษาใน Registry
!define MUI_LANGDLL_REGISTRY_ROOT "${PRODUCT_UNINST_ROOT_KEY}"
!define MUI_LANGDLL_REGISTRY_KEY "${PRODUCT_UNINST_KEY}"
!define MUI_LANGDLL_REGISTRY_VALUENAME "NSIS:Language"

; --- การกำหนดข้อความภาษาไทย (Thai Localization) ---
LangString UI_CREDIT ${LANG_THAI} "ระบบติดตั้งภาษาไทยโดย DELTA SYNTH & UTAU THAILAND"
LangString UI_WELCOME_MSG ${LANG_THAI} "ระบบจะนำคุณเข้าสู่การติดตั้ง $(^Name) ลงบนคอมพิวเตอร์ของคุณ$\r$\n$\r$\n$(UI_CREDIT)"
LangString UI_FINISH_MSG ${LANG_THAI} "การติดตั้ง $(^Name) เสร็จสมบูรณ์แล้ว"

LangString UI_CREDIT ${LANG_ENGLISH} "Installer Credit: DELTA SYNTH & Gemini AI"
LangString UI_WELCOME_MSG ${LANG_ENGLISH} "This wizard will guide you through the installation of $(^Name).$\r$\n$\r$\n$(UI_CREDIT)"
LangString UI_FINISH_MSG ${LANG_ENGLISH} "Installation of $(^Name) is complete."

; --- หน้าการติดตั้ง (Installer Pages) ---
!define MUI_WELCOMEPAGE_TEXT "$(UI_WELCOME_MSG)"
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES

; หน้าสุดท้ายเมื่อติดตั้งเสร็จ
!define MUI_FINISHPAGE_RUN "$INSTDIR\OpenUtau.exe"
!define MUI_FINISHPAGE_TEXT "$(UI_FINISH_MSG)"
!insertmacro MUI_PAGE_FINISH

; หน้าการถอนการติดตั้ง (Uninstaller Pages)
!insertmacro MUI_UNPAGE_INSTFILES

; --- รายการภาษาที่รองรับ ---
!insertmacro MUI_LANGUAGE "Thai"
!insertmacro MUI_LANGUAGE "English"
!insertmacro MUI_LANGUAGE "French"
!insertmacro MUI_LANGUAGE "German"
!insertmacro MUI_LANGUAGE "Japanese"
!insertmacro MUI_LANGUAGE "Korean"
!insertmacro MUI_LANGUAGE "Russian"
!insertmacro MUI_LANGUAGE "SimpChinese"

; --- เริ่มการทำงานของสคริปต์ ---
Name "${PRODUCT_NAME} ${PRODUCT_VERSION}"
OutFile "OpenUtau-win-${ARCH}-v1.2.exe"
InstallDir "$PROGRAMFILES64\OpenUtau"
ShowInstDetails show
ShowUnInstDetails show

Function .onInit
  !insertmacro MUI_LANGDLL_DISPLAY
FunctionEnd

Section "ส่วนหลัก (MainSection)" SEC01
  SetOutPath "$INSTDIR"
  SetOverwrite ifnewer
  ; ทำการคัดลอกไฟล์จากโฟลเดอร์ Build
  File /r "bin\win-${ARCH}\*"
SectionEnd

Section "ทางลัด (Shortcuts)"
  CreateShortCut "$SMPROGRAMS\OpenUtau.lnk" "$INSTDIR\OpenUtau.exe"
  CreateShortCut "$DESKTOP\OpenUtau.lnk" "$INSTDIR\OpenUtau.exe"
SectionEnd

Section "การลงทะเบียนระบบ (Registry & File Association)"
  WriteUninstaller "$INSTDIR\uninst.exe"
  
  ; ลงทะเบียนข้อมูลโปรแกรมใน Windows Uninstall
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "DisplayName" "$(^Name)"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "UninstallString" "$INSTDIR\uninst.exe"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "DisplayIcon" "$INSTDIR\OpenUtau.exe"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "DisplayVersion" "${PRODUCT_VERSION}"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "URLInfoAbout" "${PRODUCT_WEB_SITE}"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "Publisher" "${PRODUCT_PUBLISHER}"

  ; เชื่อมโยงนามสกุลไฟล์ .ustx กับ OpenUtau
  WriteRegStr HKCR ".ustx" "" "OpenUtauFile"
  WriteRegStr HKCR "OpenUtauFile" "" "OpenUtau Sequence File"
  WriteRegStr HKCR "OpenUtauFile\DefaultIcon" "" "$INSTDIR\OpenUtau.exe,0"
  WriteRegStr HKCR "OpenUtauFile\shell\open\command" "" '"$INSTDIR\OpenUtau.exe" "%1"'
SectionEnd

Section "VC Redist"
  SetOutPath "$TEMP"
  File "vc_redist.${ARCH}.exe"
  DetailPrint "กำลังติดตั้ง Microsoft Visual C++ Redistributable..."
  ExecWait "$TEMP\vc_redist.${ARCH}.exe /quiet /norestart"
  Delete "$TEMP\vc_redist.${ARCH}.exe"
SectionEnd

; --- ระบบถอนการติดตั้ง ---
Function un.onUninstSuccess
  HideWindow
  MessageBox MB_ICONINFORMATION|MB_OK "ถอนการติดตั้ง $(^Name) เรียบร้อยแล้ว"
FunctionEnd

Function un.onInit
  !insertmacro MUI_UNGETLANGUAGE
  MessageBox MB_ICONQUESTION|MB_YESNO|MB_DEFBUTTON2 "คุณแน่ใจหรือไม่ว่าต้องการลบ $(^Name) และส่วนประกอบทั้งหมดออกจากเครื่อง?" IDYES +2
  Abort
FunctionEnd

Section Uninstall
  Delete "$INSTDIR\uninst.exe"
  Delete "$INSTDIR\*.*"
  RMDir /r "$INSTDIR"

  Delete "$SMPROGRAMS\OpenUtau.lnk"
  Delete "$DESKTOP\OpenUtau.lnk"

  DeleteRegKey HKCR ".ustx"
  DeleteRegKey HKCR "OpenUtauFile"
  DeleteRegKey ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}"
  
  SetAutoClose true
SectionEnd
