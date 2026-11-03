#include "FocusCameraSettings.h"

UFocusCameraSettings::UFocusCameraSettings(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->DistanceMin = -1.00f;
    this->DistanceMax = -1.00f;
    this->PivotRatioToTarget = -1.00f;
    this->HeightOffset = -1.00f;
    this->LookAtOffset = -1.00f;
    this->LeftRightOffset = -1.00f;
}


