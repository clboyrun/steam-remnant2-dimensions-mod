#include "FxActorDestroyerComponent.h"

UFxActorDestroyerComponent::UFxActorDestroyerComponent(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->ForceDestroyTimer = 10.00f;
}

void UFxActorDestroyerComponent::SoftDestroyOwner() {
}

UFxActorDestroyerComponent* UFxActorDestroyerComponent::SoftDestroyActor(AActor* Actor, const TArray<FName>& InIgnoredComponents, float InForceDestroyTimer) {
    return NULL;
}

bool UFxActorDestroyerComponent::IsSoftDestroying() const {
    return false;
}


