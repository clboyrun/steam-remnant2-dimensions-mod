#include "VerticalSyncUserSetting.h"

UVerticalSyncUserSetting::UVerticalSyncUserSetting() {
    this->NameID = TEXT("VSync");
    this->Label = FText::FromString(TEXT("VSync"));
    this->Description = FText::FromString(TEXT("Synchronizes the frame rate to the display's refresh rate"));
    this->Platforms = 17;
    this->DefaultValue = true;
}


