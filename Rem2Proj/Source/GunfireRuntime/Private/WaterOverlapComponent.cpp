#include "WaterOverlapComponent.h"

UWaterOverlapComponent::UWaterOverlapComponent(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->WaterBounds = NULL;
    this->bOnlyValidIfCharacterInWater = false;
    this->bDoRedundantCheckWhileInWater = false;
    this->CachedComponent = NULL;
}

void UWaterOverlapComponent::OnEndOverlap(AActor* OverlappedActor, AActor* OtherActor) {
}

void UWaterOverlapComponent::OnEndComponentOverlap(UPrimitiveComponent* OverlappedComponent, AActor* OtherActor, UPrimitiveComponent* OtherComp, int32 OtherBodyIndex) {
}

void UWaterOverlapComponent::OnBeginOverlap(AActor* OverlappedActor, AActor* OtherActor) {
}

void UWaterOverlapComponent::OnBeginComponentOverlap(UPrimitiveComponent* OverlappedComponent, AActor* OtherActor, UPrimitiveComponent* OtherComp, int32 OtherBodyIndex, bool bFromSweep, const FHitResult& SweepResult) {
}

bool UWaterOverlapComponent::InWater() const {
    return false;
}


