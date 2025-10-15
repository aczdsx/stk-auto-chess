module Fastlane
  module Actions
    module SharedValues
    end

    class ApplyLiappIosForUnityAction < Action
      def self.run(params)
        ipa = params[:ipa]
        UI.user_error!("No ipa file given") if ipa.to_s.length == 0
        UI.user_error!("Couldn't find ipa file at path '#{File.expand_path(ipa)}'") unless File.exist?(ipa)

        signing_identity = params[:signing_identity]
        UI.user_error!("No signing identity given") if signing_identity.to_s.length == 0

        liapp_package_sender = params[:liapp_package_sender]
        UI.user_error!("Couldn't find LIAPP package sender file at path '#{File.expand_path(liapp_package_sender)}'") unless File.exist?(liapp_package_sender)

        liapp_env = params[:liapp_package_sender_env]
        UI.user_error!("Couldn't find LIAPP environment file at path '#{File.expand_path(liapp_env)}'") unless File.exist?(liapp_env)

        apply_liapp_and_resign(ipa, signing_identity, liapp_package_sender, liapp_env)
      end

      def self.apply_liapp_and_resign(application, signing_identity, liapp_package_sender, liapp_env)
        work_dir, app_basename = File.dirname(application), File.basename(application, ".*")

        # Run LiappPackageSender
        sh("java", "-jar", liapp_package_sender, "-i", application, "-o", "#{application}.liapped", "-e", liapp_env)

        # Since LiappPackageSender always returns exit code 0, check that the output file exists for safety.
        UI.user_error!("Couldn't find LIAPP application result '#{File.expand_path("#{application}.liapped")}'") unless File.exist?("#{application}.liapped")

        FileUtils.mv(application, "#{application}.origin")
        FileUtils.mv("#{application}.liapped", application)

        UI.important "Resign the application using codesign"
        UI.message "Unpack app #{app_basename}"
        sh("unzip", "-o", application)

        UI.message "Code signing step for Unity"
        if Dir.exist?("#{work_dir}/Payload/#{app_basename}.app/Frameworks/UnityFramework.framework")
          FileUtils.rm_rf("#{work_dir}/Payload/#{app_basename}.app/Frameworks/UnityFramework.framework/_CodeSignature/")
          sh("codesign", "--preserve-metadata=entitlements", "-f", "-s", signing_identity, "#{work_dir}/Payload/#{app_basename}.app/Frameworks/UnityFramework.framework/UnityFramework")
        end

        UI.message "Code signing step for app #{app_basename}"
        FileUtils.rm_rf("#{work_dir}/Payload/#{app_basename}.app/_CodeSignature")
        sh("codesign", "--preserve-metadata=entitlements", "-f", "-s", signing_identity, "#{work_dir}/Payload/#{app_basename}.app")

        UI.message "Pack resigned app #{app_basename}"
        Dir.chdir(work_dir) do
          sh("zip", "-FS", "-r", application, "Payload", "Symbols", "SwiftSupport")
        end

        UI.message "Cleanup"
        FileUtils.rm_rf("#{work_dir}/Payload")
        FileUtils.rm_rf("#{work_dir}/Symbols")
        FileUtils.rm_rf("#{work_dir}/SwiftSupport")
      end

      #####################################################
      # @!group Documentation
      #####################################################

      def self.description
        "Apply LIAPP solution and resign the application (iOS)"
      end

      def self.details
        "More information: https://docs.tech.cookapps.com/release/com.cookapps.build/manual/case-study/liapp.html"
      end

      def self.available_options
        [
          FastlaneCore::ConfigItem.new(key: :ipa,
                                       env_name: '',
                                       description: 'Path to ipa file',
                                       optional: true,
                                       default_value: Actions.lane_context[SharedValues::IPA_OUTPUT_PATH]),
          FastlaneCore::ConfigItem.new(key: :signing_identity,
                                       env_name: 'LIAPP_SIGNING_IDENTITY',
                                       description: 'Code signing identity to use'),
          FastlaneCore::ConfigItem.new(key: :liapp_package_sender,
                                       env_name: 'LIAPP_PACKAGE_SENDER_PATH',
                                       description: 'Path to LiappPackageSender file'),
          FastlaneCore::ConfigItem.new(key: :liapp_package_sender_env,
                                       env_name: 'LIAPP_PACKAGE_SENDER_ENV_PATH',
                                       description: 'Path to environment file used by LiappPackageSender')
        ]
      end

      def self.output
      end

      def self.return_value
      end

      def self.authors
        ["cookapps-devops"]
      end

      def self.is_supported?(platform)
        [:ios].include?(platform)
      end
    end
  end
end
