#include "SnapToGroundComponent.h"

USnapToGroundComponent::USnapToGroundComponent(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->SnappingEnabled = true;
    this->VerticalOffset = 100.00f;
    this->CastDistance = 100.00f;
    this->OrientToGround = false;
}


