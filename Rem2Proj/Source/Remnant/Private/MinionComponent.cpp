#include "MinionComponent.h"

UMinionComponent::UMinionComponent(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->Summoner = NULL;
    this->DisplayIcon = NULL;
}

void UMinionComponent::SetValid(bool IsMinionValid) {
}

void UMinionComponent::NotifyDisplayDataUpdated() {
}

bool UMinionComponent::IsValidMinion() {
    return false;
}

float UMinionComponent::GetSummonerStat(FName StatName) {
    return 0.0f;
}

float UMinionComponent::GetSpawnTime() {
    return 0.0f;
}

int32 UMinionComponent::GetMaxMinionsForCause(AActor* Cause, int32 Bonus) {
    return 0;
}

void UMinionComponent::DestroyMinion() {
}

void UMinionComponent::BroadcastSummonEvent(FName EventName, AActor* RelevantEventTarget) {
}


