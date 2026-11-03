#include "MusicSetAsset.h"

UMusicSetAsset::UMusicSetAsset() {
    this->Priority = 1;
}

void UMusicSetAsset::Stop_Implementation(UMusicManager* MusicManager, FMusicSetInstanceData& InstanceData, const FName& StingerName) {
}

void UMusicSetAsset::SetState_Implementation(UMusicManager* MusicManager, FMusicSetInstanceData& InstanceData, EMusicState DesiredState) {
}

void UMusicSetAsset::Play_Implementation(UMusicManager* MusicManager, FMusicSetInstanceData& InstanceData) {
}

void UMusicSetAsset::OnEvent_Implementation(UMusicManager* MusicManager, FMusicSetInstanceData& InstanceData, EMusicEvent Event) {
}


