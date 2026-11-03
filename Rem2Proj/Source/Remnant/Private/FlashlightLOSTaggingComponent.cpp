#include "FlashlightLOSTaggingComponent.h"

UFlashlightLOSTaggingComponent::UFlashlightLOSTaggingComponent(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->OverlapComponent = NULL;
}

bool UFlashlightLOSTaggingComponent::LightEndOverlap(AActor* Actor) {
    return false;
}

void UFlashlightLOSTaggingComponent::LightBeginOverlap(AActor* Actor) {
}

void UFlashlightLOSTaggingComponent::EnableOverlapCheck(UPrimitiveComponent* Primitive) {
}

void UFlashlightLOSTaggingComponent::DisableOverlapCheck() {
}


