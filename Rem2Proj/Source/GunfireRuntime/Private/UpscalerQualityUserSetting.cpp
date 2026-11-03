#include "UpscalerQualityUserSetting.h"

UUpscalerQualityUserSetting::UUpscalerQualityUserSetting() {
    this->NameID = TEXT("UpscalerQuality");
    this->Label = FText::FromString(TEXT("Upscaler Quality"));
    this->Platforms = 17;
    this->bWraps = false;
}


