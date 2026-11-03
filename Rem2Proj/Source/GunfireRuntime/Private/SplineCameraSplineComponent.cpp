#include "SplineCameraSplineComponent.h"

USplineCameraSplineComponent::USplineCameraSplineComponent(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->bAutoActivate = true;
}

FVector USplineCameraSplineComponent::GetCameraVectorAtSplineInputKey(float InputKey, TEnumAsByte<ESplineCoordinateSpace::Type> CoordinateSpace) const {
    return FVector{};
}


