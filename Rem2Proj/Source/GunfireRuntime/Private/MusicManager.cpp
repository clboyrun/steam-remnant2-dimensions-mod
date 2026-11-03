#include "MusicManager.h"

UMusicManager::UMusicManager(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->AutomatedState = EMusicState::Ambient;
    this->ManualState = EMusicState::None;
}

void UMusicManager::PostMusicEvent(EMusicEvent Event) {
}

bool UMusicManager::IsMusicSetActive(UMusicSetAsset* MusicSet, UObject* Instigator) const {
    return false;
}

UMusicManager* UMusicManager::GetMusicManager(UObject* WorldContextObject) {
    return NULL;
}

EMusicState UMusicManager::GetCurrentState() const {
    return EMusicState::None;
}

void UMusicManager::DeactivateMusicSet(UMusicSetAsset* MusicSet, UObject* Instigator, const FName& OutroStinger, bool bForceStop) {
}

void UMusicManager::ActivateMusicSet(UMusicSetAsset* MusicSet, UObject* Instigator) {
}


