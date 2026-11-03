#include "UpscalerSharpnessUserSetting.h"

UUpscalerSharpnessUserSetting::UUpscalerSharpnessUserSetting() {
    this->NameID = TEXT("UpscalerSharpness");
    this->Label = FText::FromString(TEXT("Upscaler Sharpening"));
    this->ConsoleVariable = TEXT("r.FidelityFX.FSR3.Sharpness");
    this->Platforms = 17;
    this->ShowAsPercent = false;
}


