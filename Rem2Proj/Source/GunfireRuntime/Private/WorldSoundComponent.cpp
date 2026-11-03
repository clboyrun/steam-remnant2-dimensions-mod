#include "WorldSoundComponent.h"

UWorldSoundComponent::UWorldSoundComponent(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->SoundClassOverride = NULL;
}

void UWorldSoundComponent::SetAudioList(const TArray<FWorldSound>& NewAudioList, bool bReset) {
}

void UWorldSoundComponent::ResetTimers() {
}


