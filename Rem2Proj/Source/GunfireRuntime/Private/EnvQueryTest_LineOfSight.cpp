#include "EnvQueryTest_LineOfSight.h"

UEnvQueryTest_LineOfSight::UEnvQueryTest_LineOfSight() {
    this->TestPurpose = EEnvTestPurpose::Filter;
    this->FilterType = EEnvTestFilterType::Match;
    this->ScoringEquation = EEnvTestScoreEquation::Constant;
    this->UseLastKnownPosition = true;
    this->SnapToGround = true;
    this->CustomOccluderContext = NULL;
}


