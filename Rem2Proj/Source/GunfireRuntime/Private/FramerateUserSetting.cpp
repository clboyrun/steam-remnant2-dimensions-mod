#include "FramerateUserSetting.h"

UFramerateUserSetting::UFramerateUserSetting() {
    this->NameID = TEXT("FrameRate");
    this->Label = FText::FromString(TEXT("Framerate"));
    this->Description = FText::FromString(TEXT("The maximum framerate to allow, based on the display's refresh rate"));
    this->Platforms = 17;
}


