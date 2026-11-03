#include "BreakableImpulseComponent.h"

UBreakableImpulseComponent::UBreakableImpulseComponent(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->bAutoActivate = true;
    this->ImpulseRadius = 500.00f;
    this->ImpulseStrength = 100.00f;
    this->bApplyFalloff = true;
}


