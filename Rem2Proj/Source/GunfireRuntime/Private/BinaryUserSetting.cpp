#include "BinaryUserSetting.h"

UBinaryUserSetting::UBinaryUserSetting() {
    this->OffLabel = FText::FromString(TEXT("Off"));
    this->OnLabel = FText::FromString(TEXT("On"));
    this->DefaultValue = false;
    this->bInvertConsoleVariable = false;
}

void UBinaryUserSetting::SetValue(bool Value) {
}

bool UBinaryUserSetting::GetValue() {
    return false;
}


