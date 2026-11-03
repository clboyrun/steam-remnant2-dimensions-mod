#include "SummonerComponent.h"
#include "Templates/SubclassOf.h"

USummonerComponent::USummonerComponent(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
}

void USummonerComponent::RemoveSummon(AActor* Summon) {
}

void USummonerComponent::RegisterSummon(AActor* Summon) {
}

void USummonerComponent::OnSummonEndPlay(AActor* Summon, const TEnumAsByte<EEndPlayReason::Type> EndPlayReason) {
}

bool USummonerComponent::HasSummon(TSubclassOf<AActor> SummonClass) {
    return false;
}

TArray<AActor*> USummonerComponent::GetSummonsOfType(TSubclassOf<AActor> SummonClass) {
    return TArray<AActor*>();
}

TArray<AActor*> USummonerComponent::GetSummons() {
    return TArray<AActor*>();
}

int32 USummonerComponent::GetNumSummonsOfType(TSubclassOf<AActor> SummonClass) {
    return 0;
}

void USummonerComponent::BroadcastSummonActionEvent(AActor* Summon, FName EventName, AActor* RelevantEventTarget) {
}


