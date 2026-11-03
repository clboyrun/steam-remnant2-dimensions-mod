#include "SoftCollisionBoxComponent.h"

USoftCollisionBoxComponent::USoftCollisionBoxComponent(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->ShapeBodySetup = NULL;
    this->SoftCharacterCollisionWeight = 9999.00f;
}

bool USoftCollisionBoxComponent::ShouldIgnoreActor(AActor* Actor) const {
    return false;
}

void USoftCollisionBoxComponent::SetShouldIgnoreActor(AActor* Actor, bool bShouldIgnore) {
}

void USoftCollisionBoxComponent::SetEnabled(bool bIsEnabled) {
}


