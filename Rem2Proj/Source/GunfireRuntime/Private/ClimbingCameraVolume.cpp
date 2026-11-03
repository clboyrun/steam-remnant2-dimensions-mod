#include "ClimbingCameraVolume.h"

AClimbingCameraVolume::AClimbingCameraVolume(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->bDynamicallyUpdate = true;
    this->bUseStickyMovement = false;
    this->bAllowAdditiveControls = false;
    this->bAutoDeactivateOnDismount = true;
    this->bCenterCameraOnDismount = true;
    this->CenterPitchMin = -10.00f;
    this->CenterPitchMax = 20.00f;
    this->CenterPitchYawArc = 45.00f;
}

void AClimbingCameraVolume::OnClimbSurfaceChanged(UClimbingComponent* ClimbingComponent, const FClimbableSurfaceResult& NewSurface, const FClimbableSurfaceResult& PreviousSurface) {
}


