#!/bin/bash

###############################################################################
# This script runs code coverage
###############################################################################

set -e

###############################################################################
# Define Environment Variables
###############################################################################
# Get directory of running script
DIR=$(cd "$(dirname "$0")" && pwd)

BUILD_REPOSITORY_LOCALPATH=${BUILD_REPOSITORY_LOCALPATH:-$DIR/../../..}
PROJECT_ROOT=${BUILD_REPOSITORY_LOCALPATH}/edgelet
SCRIPT_NAME=$(basename $0)

###############################################################################
# Print usage information pertaining to this script and exit
###############################################################################
usage()
{
    echo "$SCRIPT_NAME [options]"
    echo ""
    echo "options"
    echo " -h, --help          Print this help and exit."
    exit 1;
}

print_help_and_exit()
{
    echo "Run $SCRIPT_NAME --help for more information."
    exit 1
}

###############################################################################
# Obtain and validate the options supported by this script
###############################################################################
process_args()
{
    save_next_arg=0
    for arg in $@
    do
        if [ $save_next_arg -eq 1 ]; then
            TOOLCHAIN="$arg"
            save_next_arg=0
        else
            case "$arg" in
                "-h" | "--help" ) usage;;
                "-t" | "--toolchain" ) save_next_arg=1;;
                * ) usage;;
            esac
        fi
    done
}

process_args "$@"

echo "Running cargo tarpaulin"
docker run --user "$(id -u)":"$(id -g)" --rm --security-opt seccomp=unconfined -v "$PROJECT_ROOT:/volume" xd009642/tarpaulin cargo tarpaulin --out Xml
mkdir -p $PROJECT_ROOT/target
mv $PROJECT_ROOT/cobertura.xml target
