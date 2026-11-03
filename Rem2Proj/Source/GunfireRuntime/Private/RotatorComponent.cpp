#include "RotatorComponent.h"

URotatorComponent::URotatorComponent(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->AutoStart = true;
    this->SmoothTime = 0.50f;
}

void URotatorComponent::Stop(float Smooth) {
}

void URotatorComponent::Play(float Smooth) {
}


