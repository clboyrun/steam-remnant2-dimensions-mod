#include "SoftCollisionCapsuleComponent.h"

USoftCollisionCapsuleComponent::USoftCollisionCapsuleComponent(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->SoftCharacterCollisionWeight = 9999.00f;
}

bool USoftCollisionCapsuleComponent::ShouldIgnoreActor(AActor* Actor) const {
    return false;
}

void USoftCollisionCapsuleComponent::SetShouldIgnoreActor(AActor* Actor, bool bShouldIgnore) {
}

void USoftCollisionCapsuleComponent::SetEnabled(bool bIsEnabled) {
}

bool USoftCollisionCapsuleComponent::IsEnabled() const {
    return false;
}


