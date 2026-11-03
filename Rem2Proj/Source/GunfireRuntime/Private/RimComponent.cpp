#include "RimComponent.h"

URimComponent::URimComponent(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->RimFadeStartDist = 1500.00f;
    this->RimFadeFullDist = 1000.00f;
    this->RimDistanceExp = 3.00f;
    this->bUsePerComponentShading = false;
}

void URimComponent::SetupRimMaterialsForMesh(UMeshComponent* Mesh) {
}

void URimComponent::SetRimColor(FLinearColor InColor) {
}


