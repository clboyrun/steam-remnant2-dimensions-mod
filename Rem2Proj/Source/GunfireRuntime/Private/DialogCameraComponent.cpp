#include "DialogCameraComponent.h"

UDialogCameraComponent::UDialogCameraComponent(const FObjectInitializer& ObjectInitializer) : Super() {
    this->EaseTime = 0.00f;
    this->EaseType = EEaseType::EaseInOut;
    this->EaseExponent = 1.00f;
}


