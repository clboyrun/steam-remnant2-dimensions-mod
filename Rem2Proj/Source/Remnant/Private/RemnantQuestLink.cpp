#include "RemnantQuestLink.h"

URemnantQuestLink::URemnantQuestLink(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->AutoLink = false;
    this->IsMainPath = false;
    this->IsWorldMapLocation = false;
    this->AutoLinkQuestID = -1;
}

void URemnantQuestLink::OnLinkDeactivated(UQuestComponent* Component) {
}

void URemnantQuestLink::OnLinkActivated(UQuestComponent* Component) {
}


