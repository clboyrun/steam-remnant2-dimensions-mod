#include "DetailLevelComponent.h"

UDetailLevelComponent::UDetailLevelComponent(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->Priority = EDetailPriority::Normal;
}

void UDetailLevelComponent::UpdateDetailLevel(UUserSetting* AppliedSetting, UGameUserSettingsGunfire* UserSettings) {
}

void UDetailLevelComponent::SettingsRebuilt(UGameUserSettingsGunfire* UserSettings) {
}


