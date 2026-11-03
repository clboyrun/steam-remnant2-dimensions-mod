#include "UpscalerUserSetting.h"

UUpscalerUserSetting::UUpscalerUserSetting() {
    this->NameID = TEXT("Upscaler");
    this->Label = FText::FromString(TEXT("Upscaler"));
    this->Description = FText::FromString(TEXT("Boosts frame rates by rendering fewer pixels and using AI to output high resolution frames"));
    this->Platforms = 17;
}


