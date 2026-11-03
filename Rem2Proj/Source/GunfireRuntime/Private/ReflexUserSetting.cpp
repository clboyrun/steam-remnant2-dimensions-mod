#include "ReflexUserSetting.h"

UReflexUserSetting::UReflexUserSetting() {
    this->NameID = TEXT("Reflex");
    this->Label = FText::FromString(TEXT("NVIDIA Reflex Low Latency"));
    this->Description = FText::FromString(TEXT("Reflex reduces system latency and increases PC responsiveness.\n\nIn Boost mode,\x0009Reflex will attempt to optimize latency in CPU bound cases as well. This can slightly increase GPU power draw."));
    this->Platforms = 1;
}


