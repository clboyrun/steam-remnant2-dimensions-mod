#include "ZoneRegion.h"

AZoneRegion::AZoneRegion(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->Priority = 1;
}



bool AZoneRegion::Contains(const FVector& Location) {
    return false;
}


