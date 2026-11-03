#include "CompanionComponent.h"
#include "AIFollowTargetSelector.h"

UCompanionComponent::UCompanionComponent(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->bAutoActivate = true;
    this->FollowTargetSelector = UAIFollowTargetSelector::StaticClass();
    this->FollowTarget = TEXT("FollowTarget");
}

AActor* UCompanionComponent::GetCurrentFollowTarget() const {
    return NULL;
}


