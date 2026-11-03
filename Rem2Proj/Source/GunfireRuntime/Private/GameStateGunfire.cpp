#include "GameStateGunfire.h"
#include "InstancedObjectReplicationManager.h"
#include "SequencerManager.h"
#include "TimeDilationManager.h"

AGameStateGunfire::AGameStateGunfire(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->TimeDilationManager = CreateDefaultSubobject<UTimeDilationManager>(TEXT("TimeDilatonManager"));
    this->SequencerManager = CreateDefaultSubobject<USequencerManager>(TEXT("SequencerManager"));
    this->InstancedObjectRepManager = CreateDefaultSubobject<UInstancedObjectReplicationManager>(TEXT("IORepManager"));
}

void AGameStateGunfire::StartMatch() {
}

void AGameStateGunfire::RemoveGameStateInitializedListener(const FOnGameStateDelegate& Event) {
}

AGameStateGunfire* AGameStateGunfire::GetGunfireGameState(const UObject* WorldContextObject) {
    return NULL;
}

void AGameStateGunfire::AddGameStateInitializedListener(const FOnGameStateDelegate& Event) {
}


