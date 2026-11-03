#include "ExplorableMinimapComponent.h"

UExplorableMinimapComponent::UExplorableMinimapComponent(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->ActorClass = NULL;
    this->bFaceCamera = true;
    this->bIsSecret = false;
    this->RevealRange = 0.00f;
    this->RevealRangeZ = 100.00f;
    this->MaterialParamCollection = NULL;
    this->MaterialParamCollectionInst = NULL;
}

void UExplorableMinimapComponent::SetIsSecret(bool InIsSecret) {
}

AActor* UExplorableMinimapComponent::GetSpawnedActor() {
    return NULL;
}


