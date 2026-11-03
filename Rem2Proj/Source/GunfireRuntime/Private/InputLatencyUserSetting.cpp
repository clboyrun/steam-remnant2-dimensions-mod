#include "InputLatencyUserSetting.h"

UInputLatencyUserSetting::UInputLatencyUserSetting() {
    this->NameID = TEXT("InputLag");
    this->Label = FText::FromString(TEXT("Minimize Input Latency"));
    this->Description = FText::FromString(TEXT("Improve input latency at the cost of a small performance loss"));
    this->Platforms = 17;
}


