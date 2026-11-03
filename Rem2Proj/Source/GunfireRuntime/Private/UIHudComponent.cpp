#include "UIHudComponent.h"

UUIHudComponent::UUIHudComponent(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->bVisible = true;
}

void UUIHudComponent::SetVisibility(bool Visible) {
}

void UUIHudComponent::OnParentSetVisibility_Implementation(bool Visible) {
}

bool UUIHudComponent::IsVisible() const {
    return false;
}

APawn* UUIHudComponent::GetControllingPawn() const {
    return NULL;
}


