#include "WaterSurfaceState.h"

UWaterSurfaceState::UWaterSurfaceState() {
    this->bDisableCollisionOnEntry = false;
    this->TraversalType = EWaterSurfaceTraversalType::Emerge;
}


