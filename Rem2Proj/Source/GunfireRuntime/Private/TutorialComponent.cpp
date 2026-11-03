#include "TutorialComponent.h"

UTutorialComponent::UTutorialComponent(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->TutorialPrompts = NULL;
}

void UTutorialComponent::SetTutorialPromptOverride_Implementation(const FName& ID, AActor* Actor, const FText& Text, float DurationOverride) {
}

void UTutorialComponent::SetTutorialPrompt_Implementation(const FName& ID, AActor* Actor, float DurationOverride) {
}

void UTutorialComponent::RemoveTutorialPrompt_Implementation(const FName& ID, AActor* Actor) {
}

void UTutorialComponent::QueueTutorialPrompt_Implementation(const FName& ID, AActor* Actor, float DurationOverride) {
}

bool UTutorialComponent::HasActiveTutorialPrompt(AActor* Actor, FName RequiredID) {
    return false;
}

void UTutorialComponent::ClearTutorialPrompts(AActor* Actor) {
}

bool UTutorialComponent::AreTutorialsEnabled() {
    return false;
}


