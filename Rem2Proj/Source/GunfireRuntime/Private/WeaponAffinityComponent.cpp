#include "WeaponAffinityComponent.h"

UWeaponAffinityComponent::UWeaponAffinityComponent(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->Affinities = NULL;
}

float UWeaponAffinityComponent::ComputeScalar() {
    return 0.0f;
}


