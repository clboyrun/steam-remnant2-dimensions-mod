#include "AIDataRenderingComponent.h"

UAIDataRenderingComponent::UAIDataRenderingComponent(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->bCanEverAffectNavigation = false;
    this->bIsEditorOnly = true;
    this->Mobility = EComponentMobility::Stationary;
}


