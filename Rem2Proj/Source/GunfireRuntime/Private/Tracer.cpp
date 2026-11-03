#include "Tracer.h"

ATracer::ATracer(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->Speed = 2000.00f;
    this->MaxFlightTime = 0.00f;
}

void ATracer::InitFromCause_Implementation(AActor* Cause) {
}


