#include "AIDataComponent.h"

UAIDataComponent::UAIDataComponent(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->bCanEverAffectNavigation = true;
    this->AIData = NULL;
}

void UAIDataComponent::SetState(bool Enabled) {
}


