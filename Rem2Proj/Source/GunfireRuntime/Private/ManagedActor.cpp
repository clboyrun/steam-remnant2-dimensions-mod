#include "ManagedActor.h"

AManagedActor::AManagedActor(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->EnableAnimOptimizations = true;
    this->EnableAnimSignificanceOptimizations = true;
    this->AnimSignificanceScalar = 1.00f;
    this->SuspendClothWhenNotVisible = true;
    this->EnableTickRateOptimizations = false;
}

FString AManagedActor::GetDebugInfo(int32 DebugLevel) {
    return TEXT("");
}


