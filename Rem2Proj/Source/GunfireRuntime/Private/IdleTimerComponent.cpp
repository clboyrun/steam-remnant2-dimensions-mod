#include "IdleTimerComponent.h"

UIdleTimerComponent::UIdleTimerComponent(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->Timeout = 180.00f;
    this->TimeoutUpdateInterval = 1.00f;
    this->IdleTime = 0.00f;
}

void UIdleTimerComponent::Ping() {
}


