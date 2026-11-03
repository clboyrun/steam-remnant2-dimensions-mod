#include "NavigationSystemGunfire.h"

UNavigationSystemGunfire::UNavigationSystemGunfire() {
    this->DefaultAgentName = TEXT("Default");
    this->bShouldDiscardSubLevelNavData = false;
    this->SupportedAgents.AddDefaulted(5);
}


