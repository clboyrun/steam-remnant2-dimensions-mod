#include "MusicDirector.h"
#include "Net/UnrealNetwork.h"

UMusicDirector::UMusicDirector(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->MusicIntensity = EMusicIntensity::Ambient;
}

void UMusicDirector::OnRep_MusicIntensity() {
}

void UMusicDirector::GetLifetimeReplicatedProps(TArray<FLifetimeProperty>& OutLifetimeProps) const {
    Super::GetLifetimeReplicatedProps(OutLifetimeProps);
    
    DOREPLIFETIME(UMusicDirector, MusicIntensity);
}


