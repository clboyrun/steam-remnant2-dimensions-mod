#include "UIHudTutorialComponent.h"

UUIHudTutorialComponent::UUIHudTutorialComponent(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->Arrow = NULL;
    this->Border = 64.00f;
    this->IconSize = 64.00f;
    this->FadeInTime = 0.25f;
    this->FadeOutTime = 0.25f;
    this->ShowPlayerPrompts = true;
    this->PlayerPromptWidget = NULL;
    this->CachedTutorialComponent = NULL;
}

void UUIHudTutorialComponent::OnShowPrompt(FName ID, AActor* Actor, const FTutorialPrompt& Prompt, float ExpirationTime) {
}

void UUIHudTutorialComponent::OnHidePrompt(FName ID, AActor* Actor, const FTutorialPrompt& Prompt, float ExpirationTime) {
}

void UUIHudTutorialComponent::OnClearPrompt(FName ID, AActor* Actor, const FTutorialPrompt& Prompt, float ExpirationTime) {
}


