#include "PlacementHelperComponent.h"

UPlacementHelperComponent::UPlacementHelperComponent(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->bIsEditorOnly = true;
    this->AlignTolerance = 10.00f;
    this->AlignRadius = 0.00f;
    this->DesiredHeight = 0.00f;
    this->bUpdateOnLevelLoad = false;
}

void UPlacementHelperComponent::OnPostEditMove(bool bFinished) {
}


