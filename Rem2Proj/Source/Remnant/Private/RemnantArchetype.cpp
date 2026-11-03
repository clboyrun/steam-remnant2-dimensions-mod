#include "RemnantArchetype.h"

URemnantArchetype::URemnantArchetype() {
    this->Difficulty = 0;
    this->Image = NULL;
}

TSoftClassPtr<AItem> URemnantArchetype::GetEngramItem(TSoftClassPtr<URemnantArchetype> ArchetypeSoft) {
    return NULL;
}

TSoftClassPtr<UArchetype> URemnantArchetype::GetArchetypeTrait(TSoftClassPtr<URemnantArchetype> ArchetypeSoft) {
    return NULL;
}


